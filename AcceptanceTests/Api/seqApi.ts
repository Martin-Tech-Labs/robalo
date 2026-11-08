import request from 'supertest';
import Config from '../Config/config.ts';


const SeqApi = {
    events(filter: string, count: number = 1) {
        return request(Config.SeqUrl)
            .get('/api/events')
            .query({
                filter: filter,
                count: count
            })
    },
};

export default SeqApi;