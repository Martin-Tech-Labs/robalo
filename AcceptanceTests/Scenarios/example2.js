require('chai').should();

describe('Some feature', () => {

    describe('Some Scenario', () => {

        let number = 2;

        it('Given a number', () => {
            number.should.exist;
        });
        it('And that number is 2', () => {
            number.should.equal(2);
        });

        it('When adding 40', () => {
            number += 40;
        });

        it('Then the number should be 42', () => {
            number.should.equal(42);
        });
    });
});